# HitTheKit CLA adoption checklist

> **DRAFT PROCESS — NOT IN FORCE — NOT LEGALLY REVIEWED**
>
> This checklist does not activate the accompanying CLA draft, request a
> signature, or authorize substantial external contributions.

## Selected planning model

The current planning candidate is the Harmony Combined Contributor Agreement
Template 1.0 configured as:

- copyright license, not copyright assignment;
- contributor retains copyright;
- broad sublicensable copyright grant;
- patent grant;
- outbound licensing based on Harmony Option Five; and
- separate treatment of individual and Legal Entity contributors still to be
  decided.

Option Five is being evaluated because it permits open-source and commercial
or proprietary outbound licenses while promising that an incorporated
Contribution is also available under the community license used for the
Material. Its breadth is not yet approved.

## Blocking legal and identity decisions

- [ ] Obtain review from a qualified lawyer, FOSS legal clinic, or equivalent
      professional familiar with contributor agreements and dual licensing.
- [ ] Verify the legal identity and capacity of the party receiving the grant.
- [ ] Decide whether the recipient is an individual, company, foundation, or
      another legal structure.
- [ ] Record the recipient's legal address and a private legal contact.
- [ ] Select governing law, forum, and dispute process with qualified advice.
- [ ] Verify chain of title and authority for the existing HitTheKit code.
- [ ] Confirm the intended MPL-2.0 transition separately; the CLA must not be
      used to imply that a repository license change has already occurred.
- [ ] Review the scope of “transferable,” “irrevocable,” “otherwise exploit,”
      multi-tier sublicensing, and future commercial/proprietary licensing.
- [ ] Review patent scope, Affiliate coverage, subsequently acquired claims,
      combination claims, limitations, and termination/retaliation.
- [ ] Decide whether and how to address moral rights without an unnecessarily
      broad waiver.
- [ ] Draft jurisdiction-appropriate warranty, disclaimer, liability,
      severability, assignment, notices, and entire-agreement terms.

## Contributor types and authority

- [ ] Decide whether to publish separate Individual and Entity agreements.
- [ ] Define when an individual contributor needs employer consent.
- [ ] Define acceptable evidence of employer consent.
- [ ] Define the Entity signatory's required authority and title.
- [ ] Decide whether entities maintain a list of authorized contributors.
- [ ] Define how an entity adds or removes authorized contributors.
- [ ] Decide how contractors, students, public employees, and grant-funded
      contributors are handled.
- [ ] Decide whether contributions from minors will be accepted.
- [ ] If minors may contribute, obtain qualified advice on age, guardian
      consent, identity verification, privacy, and revocation.

## Contributions and third-party material

- [ ] Define the channels that count as intentionally “Submitted.”
- [ ] Define a clear “Not a Contribution” mechanism.
- [ ] Keep issues, discussions, hardware observations, ideas, and facts outside
      contractual acceptance unless qualified review says otherwise.
- [ ] Define a non-owner/mixed-submission review process—or continue to reject
      all mixed submissions.
- [ ] Explicitly reject unapproved third-party code, music, recordings,
      performances, charts, MIDI captures, artwork, fonts, trademarks,
      confidential information, and personal data.
- [ ] Require provenance and license evidence for any exceptional third-party
      material accepted through a separate process.
- [ ] Confirm that a CLA grants only rights the contributor controls and never
      substitutes for third-party licenses.

## Existing and future contributions

- [ ] Inventory any substantial external contribution made before activation.
- [ ] Decide whether prior contributors must separately opt in.
- [ ] Do not presume retroactive acceptance based on an old pull request,
      commit, discussion, or repository license.
- [ ] Define the effective date for each contributor and Contribution.
- [ ] Define treatment when the community license changes after acceptance.
- [ ] Define the procedure if the receiving legal entity changes.
- [ ] Define a transparent amendment process and when re-acceptance is needed.

## Privacy, signatures, and records

- [ ] Publish a privacy notice before collecting acceptances.
- [ ] Identify the data controller, purposes, lawful basis, processors,
      transfers, and contact.
- [ ] Decide which identity, name, email, employer, timestamp, IP address, and
      other evidence is actually necessary; minimize collection.
- [ ] Define retention periods rather than retaining personal data by default.
- [ ] Define access, correction, objection, deletion, export, and complaint
      handling as applicable.
- [ ] Record the exact CLA version and immutable digest accepted.
- [ ] Preserve an auditable timestamp and authenticated contributor identity.
- [ ] Design the electronic signature/acceptance action with qualified review.
- [ ] Define recovery, backup, access control, incident response, and processor
      exit procedures for acceptance records.

## CLA Assistant and repository controls

- [ ] Complete legal and privacy review **before** enabling CLA Assistant.
- [ ] Publish only the approved, versioned CLA text—not this draft—as the
      acceptance source.
- [ ] Review CLA Assistant's hosting, authentication, custom fields, privacy,
      retention, availability, and version-change behavior.
- [ ] Configure a test repository before connecting HitTheKit.
- [ ] Verify individual and Entity workflows, bot comments, re-signing, and
      failure/recovery behavior.
- [ ] Add the CLA status check to branch protection only after a successful
      test run and with a unique check name.
- [ ] Keep maintainer bypass disabled or document narrowly reviewed emergency
      handling.
- [ ] Update the pull-request template without claiming that opening a PR
      constitutes acceptance.
- [ ] Update `CONTRIBUTING.md`, `GOVERNANCE.md`, privacy documentation, and the
      public website only when the agreement actually enters into force.
- [ ] Publish a plain-language announcement of the effective date, scope,
      contributor choices, community-license promise, and contact route.
- [ ] Confirm substantial external contributions remain blocked until every
      activation item is complete.

GitHub required status checks enforce workflow results; they do not determine
whether the CLA text or acceptance process is legally sufficient. CLA
Assistant automates prompts, GitHub authentication, acceptance records, and a
pull-request status check, but it is not the legal reviewer or receiving party.

## DCO, CLA, and CLA Assistant are different

| Mechanism | Primary function | What it does not establish by itself |
| --- | --- | --- |
| Developer Certificate of Origin (DCO) | Contributor attests provenance and a right to submit under the indicated open-source terms, commonly recorded with `Signed-off-by` | It does not grant HitTheKit the separate, broad sublicensing rights needed for proprietary/commercial outbound licensing. |
| Contributor License Agreement (CLA) | A contract can expressly grant copyright and patent rights while the contributor retains ownership | A draft is not operative without identified parties, reviewed terms, valid acceptance, and appropriate records. |
| CLA Assistant | Automates the presentation and collection of a project's chosen CLA and reports status to GitHub | It does not draft, approve, interpret, or make legally sufficient the CLA and privacy process. |

MPL 2.0 grants rights from contributors to recipients of MPL-covered software,
but its contributor grant does not automatically appoint the HitTheKit
maintainer to relicense every external Contribution under unrelated proprietary
terms. The proposed CLA is intended to address that separate inbound-rights
need if qualified review approves it.

## Review findings to resolve before adoption

### P0

None identified in a non-operative documentation draft.

### P1 — adoption blockers

- The receiving legal party, governing law, forum, legal contact, privacy
  controller, and acceptance process are unknown.
- Harmony Option Five and the copyright grant are deliberately broad; their
  sublicensing, transferability, irrevocability, and proprietary-license scope
  require qualified review and plain-language disclosure.
- Patent scope and any retaliation/termination mechanism are unresolved.
- Individual, Entity, employer-consent, Affiliate, and minor-contributor
  processes are unresolved.
- The public `main` license and the intended MPL-2.0 state are temporarily
  different; activation before the community-license transition is settled
  would create avoidable ambiguity.
- Privacy, retention, version evidence, electronic signature, and treatment of
  prior Contributions are unresolved.

### P2 — process and clarity risks

- One combined draft is convenient for review but separate Individual and
  Entity forms may be clearer operationally.
- Media and third-party-content rules may need their own contributor guide and
  provenance workflow rather than additional breadth in the CLA.
- A CLA tool outage, Gist change, account rename, or bot-status ambiguity needs
  an operational recovery procedure.

### P3 — editorial follow-up

- Defined terms, capitalization, translations, accessibility, and
  plain-language summaries should be normalized after legal text stabilizes.
- A version identifier and digest format should be selected before publication.

## Pro bono review request template

Subject: Limited pro bono review request — open-source contributor agreement

> Hello,
>
> I maintain HitTheKit, an open-source electronic-drum learning game. The
> project is preparing to use Mozilla Public License 2.0 for its community
> edition and may later offer separate commercial licenses. I would like
> contributors to retain their copyright while granting the project the
> copyright and patent permissions needed to distribute accepted contributions
> under MPL 2.0 and, transparently, under separate commercial/proprietary terms.
>
> I have prepared a non-operative draft based on the Harmony Agreements
> copyright-license model and outbound Option Five. The project is not yet
> accepting substantial external code or assets, no one is being asked to sign
> the draft, and CLA Assistant has not been configured.
>
> I currently have no budget for a full commercial engagement. Would your
> clinic, practice, or open-source legal community be willing to provide a
> limited pro bono review of the draft, focusing on the receiving party,
> sublicensing scope, patent grant, employer/entity contributions, governing
> law, privacy and electronic acceptance? I can provide the short draft,
> adoption checklist, current license documents, and a concise list of open
> questions.
>
> I understand that availability may be limited and that an informal community
> response may not create a lawyer-client relationship or replace advice from a
> qualified professional in the relevant jurisdiction.

## Primary drafting references

- Harmony agreement templates and guide:
  <https://www.harmonyagreements.org/agreements>
- Mozilla Public License 2.0:
  <https://www.mozilla.org/MPL/2.0/>
- Developer Certificate of Origin 1.1:
  <https://developercertificate.org/>
- CLA Assistant project documentation:
  <https://github.com/cla-assistant/cla-assistant>
- GitHub protected branches and required status checks:
  <https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches>

This checklist is engineering and governance preparation, not legal advice.
